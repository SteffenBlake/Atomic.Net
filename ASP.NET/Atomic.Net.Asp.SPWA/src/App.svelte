<script lang="ts">
    import * as formatMsg from "format-message";
    import { apiClient } from "./httpClient";
    import { isAxiosError } from "axios";
    let getFooId: number = 1;
    let getFoo: Promise<any>;

    function onGetFoo() {
        getFoo = getFooCmd();
    }

    async function getFooCmd(): Promise<any> {
        try {
            const res = await apiClient.get(`/api/foos/${getFooId}`);
            return res.data;
        } catch (error: unknown) {
            if (isAxiosError(error)) {
                throw error.response?.data ?? error;
            }
            throw error;
        }
    }
    
    let deleteFooId: number = 1;
    let deleteFoo: Promise<any>;

    function onDeleteFoo() {
        deleteFoo = deleteFooCmd();
    }

    async function deleteFooCmd(): Promise<any> {
        try {
            const res = await apiClient.delete(`/api/foos/${deleteFooId}`);
            return res.data;
        } catch (error: unknown) {
            if (isAxiosError(error)) {
                throw error.response?.data ?? error;
            }
            throw error;
        }
    }

    function format(error: any): string {
        var detail = formatMsg.default(error.detail, error.args);
        return `${error.fieldName} ${detail}`;
    }
</script>

<main>
    <form on:submit|preventDefault={onGetFoo} aria-label="Get Foo form">
        <label for="get-foo-id-input">
            Get Foo ID:
            <input
                data-testid="get-foo-id-input"
                type="number"
                bind:value={getFooId}
                min="1"
            />
        </label>
        <button type="submit" data-testid="get-foo-submit-btn">Submit</button>
    </form>

    {#if getFoo}
        {#await getFoo}
            <span role="status" aria-live="polite">Loading...</span>
        {:then data}
            <div role="region" aria-label="Foo data">
                <span
                    >ID: <span data-testid="get-foo-result-id">{data.id}</span
                    ></span
                >
                <br />
                <span
                    >Full Name: <span data-testid="get-foo-result-fullname"
                        >{data.fullName}</span
                    ></span
                >
                <br />
                <span
                    >Sensitive Data: <span data-testid="get-foo-result-sensitive"
                        >{data.sensitiveData}</span
                    ></span
                >
            </div>
        {:catch error}
            {#if error.title}
                <div data-testid="get-foo-validation-errors">
                    <span>{error.title}</span><br />
                    <div>
                        {#each error.errors as errorDetail, n}
                            <span data-testid="get-foo-validation-errors-details-{n}">
                                {format(errorDetail)}
                            </span>
                        {/each}
                    </div>
                </div>
            {:else if error.message}
                <div role="alert">
                    <span data-testid="get-foo-server-errors">
                        {error.message}
                    </span>
                </div>
            {/if}
        {/await}
    {/if}

    <hr/>

    <form on:submit|preventDefault={onDeleteFoo} aria-label="Foo delete form">
        <label for="delete-foo-id-input">
            Delete Foo ID:
            <input
                data-testid="delete-foo-id-input"
                type="number"
                bind:value={deleteFooId}
                min="1"
            />
        </label>
        <button type="submit" data-testid="delete-foo-submit-btn">Submit</button>
    </form>
    
    {#if deleteFoo}
        {#await deleteFoo}
            <span role="status" aria-live="polite">Loading...</span>
        {:then data}
            <span data-testid="delete-foo-success">Success!</span>
        {:catch error}
            {#if error.title}
                <div data-testid="delete-foo-validation-errors">
                    <span>{error.title}</span><br />
                    <div>
                        {#each error.errors as errorDetail, n}
                            <span data-testid="delete-foo-validation-errors-details-{n}">
                                {format(errorDetail)}
                            </span>
                        {/each}
                    </div>
                </div>
            {:else if error.message}
                <div role="alert">
                    <span data-testid="delete-foo-server-errors">
                        {error.message}
                    </span>
                </div>
            {/if}
        {/await}
    {/if}
</main>
